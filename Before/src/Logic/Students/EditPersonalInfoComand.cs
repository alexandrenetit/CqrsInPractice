using CSharpFunctionalExtensions;
using Logic.Dtos;
using Logic.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Students
{
    public interface ICommand
    {
    }

    public interface IQuery<TResult>
    {
    }

    public interface ICommandHandler<TCommand>
       where TCommand : ICommand
    {
        Result Handle(TCommand command);
    }

    public interface IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        TResult Handle(TQuery query);
    }

    public sealed class GetListQuery : IQuery<List<StudentDto>>
    {
        public string EnrolledIn { get; }
        public int? NumberOfCourses { get; }

        public GetListQuery(string enrolledIn, int? numberOfCourses)
        {
            EnrolledIn = enrolledIn;
            NumberOfCourses = numberOfCourses;
        }
    }

    public sealed class GetListQueryHandler : IQueryHandler<GetListQuery, List<StudentDto>>
    {
        private readonly UnitOfWork _unitOfWork;

        public GetListQueryHandler(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public List<StudentDto> Handle(GetListQuery query)
        {
            return new StudentRepository(_unitOfWork)
                .GetList(query.EnrolledIn, query.NumberOfCourses)
                .Select(x => ConvertToDto(x))
                .ToList();
        }

        private StudentDto ConvertToDto(Student student)
        {
            return new StudentDto
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Course1 = student.FirstEnrollment?.Course?.Name,
                Course1Grade = student.FirstEnrollment?.Grade.ToString(),
                Course1Credits = student.FirstEnrollment?.Course?.Credits,
                Course2 = student.SecondEnrollment?.Course?.Name,
                Course2Grade = student.SecondEnrollment?.Grade.ToString(),
                Course2Credits = student.SecondEnrollment?.Course?.Credits,
            };
        }
    }

    public sealed class EditPersonalInfoComand : ICommand
    {
        public long Id { get; }
        public string Name { get; }
        public string Email { get; }

        public EditPersonalInfoComand(long id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
        }
    }

    public sealed class EditPersonalInfoCommandHandler : ICommandHandler<EditPersonalInfoComand>
    {
        private readonly UnitOfWork _unitOfWork;

        public EditPersonalInfoCommandHandler(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Result Handle(EditPersonalInfoComand command)
        {
            var repository = new StudentRepository(_unitOfWork);
            Student student = repository.GetById(command.Id);

            if (student == null)
                return Result.Fail($"No student found for Id {command.Id}");

            student.Name = command.Name;
            student.Email = command.Email;

            _unitOfWork.Commit();

            return Result.Ok();
        }
    }

    public sealed class RegisterCommand : ICommand
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Course1 { get; set; }
        public string Course1Grade { get; set; }
        public string Course2 { get; set; }
        public string Course2Grade { get; set; }

        public RegisterCommand(string name, string email, string course1, string course1Grade, string course2, string course2Grade)
        {
            Name = name;
            Email = email;
            Course1 = course1;
            Course1Grade = course1Grade;
            Course2 = course2;
            Course2Grade = course2Grade;
        }
    }

    public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand>
    {
        private readonly UnitOfWork _unitOfWork;

        public RegisterCommandHandler(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Result Handle(RegisterCommand command)
        {
            var courseRepository = new CourseRepository(_unitOfWork);
            var studentRepository = new StudentRepository(_unitOfWork);
            var student = new Student(command.Name, command.Email);

            if (command.Course1 !=  null && command.Course1Grade != null)
            {
                Course course = courseRepository.GetByName(command.Course1);
                student.Enroll(course, Enum.Parse<Grade>(command.Course1Grade));
            }

            if (command.Course2 != null && command.Course2Grade != null)
            {
                Course course = courseRepository.GetByName(command.Course2);
                student.Enroll(course, Enum.Parse<Grade>(command.Course2Grade));
            }

            studentRepository.Save(student);
            _unitOfWork.Commit();

            return Result.Ok();
        }

        public sealed class UnregisterCommand : ICommand 
        {
            public long Id { get; }

            public UnregisterCommand(long id)
            {
                Id = id;
            }
        }

        public sealed class UnregisterCommandHandler : ICommandHandler<UnregisterCommand>
        {
            private readonly UnitOfWork _unitOfWork;

            public UnregisterCommandHandler(UnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public Result Handle(UnregisterCommand command)
            {
                var repository = new StudentRepository(_unitOfWork);
                Student student = repository.GetById(command.Id);
                if (student == null)
                    return Result.Fail($"No student found for Id {command.Id}");

                repository.Delete(student);
                _unitOfWork.Commit();

                return Result.Ok();
            }
        }
    }
}